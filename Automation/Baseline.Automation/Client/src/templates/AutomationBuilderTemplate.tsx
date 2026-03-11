import React, {
  forwardRef,
  useCallback,
  useEffect,
  useImperativeHandle,
  useMemo,
  useRef,
  useState,
} from "react";
import {
  RoutingContentPlaceholder,
  usePageCommandProvider,
} from "@kentico/xperience-admin-base";
import {
  Box,
  Button,
  ButtonColor,
  ButtonSize,
  ButtonType,
  Checkbox,
  Condition,
  ConditionBuilder,
  ConditionGroup,
  ConditionGroupOverview,
  ConditionOperatorLine,
  ConditionPicker,
  Icon,
  Inline,
  Input,
  MenuItem,
  Select,
  Spacing,
  TestIds,
} from "@kentico/xperience-admin-components";
import {
  ReactFlow,
  Background,
  Controls,
  useNodesState,
  useEdgesState,
  Handle,
  Position,
  EdgeLabelRenderer,
  BaseEdge,
  type Node,
  type Edge,
  MarkerType,
  type NodeTypes,
  type NodeProps,
  type EdgeTypes,
  type EdgeProps,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import styles from "./AutomationBuilderTemplate.module.css";

// Runtime-only exports — present in the SystemJS bundle but not in type declarations
const {
  Portal,
  TemplateSideMenu,
  ConfirmationDialog,
  FormSubmissionStatus,
  useEditableObjectStatusObservee,
} = require("@kentico/xperience-admin-base") as {
  Portal: React.ComponentType<{
    container: HTMLElement | null;
    children: React.ReactNode;
  }>;
  TemplateSideMenu: React.ComponentType<Record<string, never>>;
  ConfirmationDialog: React.ComponentType<{
    headline: string;
    confirmationButtonLabel: string;
    isConfirmationButtonDestructive: boolean;
    onCancellation: () => void;
    onConfirmation: () => void;
    actionInProgress?: boolean;
    children?: React.ReactNode;
  }>;
  FormSubmissionStatus: {
    Error: string;
    ValidationFailure: string;
    ValidationSuccess: string;
    ConfirmationValidationFailure: string;
  };
  useEditableObjectStatusObservee: () => {
    setDataChanged: (id: string, changed: boolean) => void;
    getNewId: () => string;
  };
};

/* ------------------------------------------------------------------ */
/*  Shared types — match server DTOs                                   */
/* ------------------------------------------------------------------ */

interface AutomationProcessNodeDto {
  id: string;
  name: string;
  stepType: string;
  iconName: string;
  iconTooltip?: string | null;
  isSaved: boolean;
  statistics: AutomationProcessNodeStatisticDto[];
  configuration?: Record<string, unknown> | null;
}

interface AutomationProcessNodeStatisticDto {
  iconName: string;
  value: number;
  statisticTooltip?: string | null;
}

interface AutomationProcessConnectionDto {
  id: string;
  sourceNodeGuid: string;
  targetNodeGuid: string;
  sourceHandle?: string | null;
}

interface LoadAutomationProcessResult {
  nodes: AutomationProcessNodeDto[];
  connections: AutomationProcessConnectionDto[];
}

interface SetIsAutomationProcessEnabledResult {
  isEnabled: boolean;
}

interface AutomationBuilderSaveResult {
  status: string;
}

interface StepFormFieldOption {
  value: string;
  label: string;
  editUrl?: string | null;
}

interface StepFormFieldDefinition {
  name: string;
  label: string;
  fieldType: string; // text, number, select, checkbox, datetime, codename, radio, conditionBuilder, objectSelector, combobox
  required: boolean;
  defaultValue?: string | null;
  placeholder?: string | null;
  options?: StepFormFieldOption[] | null;
  editUrl?: string | null;
  helpText?: string | null;
  visibleWhen?: { fieldName: string; value: string } | null;
}

interface GetStepFormDefinitionResult {
  stepType: string;
  headline: string;
  fields: StepFormFieldDefinition[];
}

interface StepTypeTileItem {
  stepType: string;
  label: string;
  iconName: string;
  description?: string | null;
}

interface GetStepTypeTileItemsResult {
  items: StepTypeTileItem[];
}

interface TriggerTypeTileItem {
  triggerType: string;
  label: string;
  iconName: string;
  description?: string | null;
}

interface GetTriggerTypeTileItemsResult {
  items: TriggerTypeTileItem[];
}

interface ConditionRuleParameter {
  name: string;
  caption: string;
  controlType: string; // dropdown, text, number
  placeholder?: string;
  defaultValue?: string;
  options?: { value: string; label: string }[];
}

interface ConditionRuleItem {
  value: string;
  label: string;
  categoryId: string;
  ruleText: string;
  parameters: ConditionRuleParameter[];
}

interface SelectedCondition {
  ruleItem: ConditionRuleItem;
  paramValues: Record<string, string>;
}

interface ConditionRuleCategory {
  id: string;
  label: string;
}

interface GetConditionRulesResult {
  items: ConditionRuleItem[];
  categories: ConditionRuleCategory[];
}

interface EmailSelectorItem {
  guid: string;
  name: string;
  purpose: string;
  channelName: string;
  status: string;
}

interface EmailChannelOption {
  id: number;
  name: string;
}

interface GetEmailsForSelectorResult {
  emails: EmailSelectorItem[];
  channels: EmailChannelOption[];
}

interface SubmitButtonProperties {
  label: string;
  tooltipText?: string | null;
  confirmationDialog?: {
    button: string;
    title: string;
    detail: string;
  } | null;
}

/* ------------------------------------------------------------------ */
/*  Template props — from AutomationBuilderClientProperties.cs         */
/* ------------------------------------------------------------------ */

interface AutomationBuilderProps {
  isAutomationProcessEnabled: boolean;
  isEditingAllowed: boolean;
  hasHistoryData: boolean;
  saveButton: SubmitButtonProperties;
}

/* ------------------------------------------------------------------ */
/*  Custom hook — automation page commands                             */
/* ------------------------------------------------------------------ */

function useAutomationCommands() {
  const { executeCommand } = usePageCommandProvider();

  const loadAutomationProcess = useCallback(async () => {
    const result = await executeCommand<LoadAutomationProcessResult>(
      "LoadAutomationProcess",
    );
    if (result) {
      return { nodes: result.nodes, connections: result.connections };
    }
    return null;
  }, [executeCommand]);

  const enableAutomationProcess = useCallback(() => {
    return executeCommand<SetIsAutomationProcessEnabledResult>(
      "EnableAutomationProcess",
    );
  }, [executeCommand]);

  const disableAutomationProcess = useCallback(() => {
    return executeCommand<SetIsAutomationProcessEnabledResult>(
      "DisableAutomationProcess",
    );
  }, [executeCommand]);

  return {
    loadAutomationProcess,
    enableAutomationProcess,
    disableAutomationProcess,
  };
}

/* ------------------------------------------------------------------ */
/*  Constants                                                          */
/* ------------------------------------------------------------------ */

const NODE_WIDTH = 260;
const NODE_HEIGHT = 62;
const NODE_SPACING_Y = 60;

/* ------------------------------------------------------------------ */
/*  React Flow custom node types                                       */
/* ------------------------------------------------------------------ */

interface StepNodeData {
  label: string;
  iconName: string;
  stepType: string;
  disabled: boolean;
  isTrigger: boolean;
  hasChildren: boolean;
  onDelete: (id: string) => void;
  onClick: (id: string) => void;
  [key: string]: unknown;
}

function StepNodeComponent({ id, data }: NodeProps<Node<StepNodeData>>) {
  const [menuOpen, setMenuOpen] = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!menuOpen) return;
    const close = (e: MouseEvent) => {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) {
        setMenuOpen(false);
      }
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [menuOpen]);

  return (
    <div className={styles.nodeWrapper}>
      {!data.isTrigger && <Handle type="target" position={Position.Top} />}
      {data.isTrigger && (
        <div className={styles.startingLineLabel}>Starting line</div>
      )}
      <div
        className={`${styles.stepNode} ${data.disabled ? styles.stepNodeDisabled : ""}`}
        onClick={() => !data.disabled && data.onClick(id)}
      >
        <span className={styles.iconBorder}>
          <Icon name={data.iconName as any} />
        </span>
        <span>{data.label}</span>
        {!data.disabled && (
          <div className={styles.menuBtnWrap} ref={wrapRef}>
            <button
              className={styles.menuBtn}
              onClick={(e) => {
                e.stopPropagation();
                setMenuOpen((v) => !v);
              }}
              title="Options"
            >
              <Icon name="xp-ellipsis" />
            </button>
            {menuOpen && (
              <div className={styles.menuPopup}>
                <button
                  className={styles.menuItem}
                  onClick={(e) => {
                    e.stopPropagation();
                    setMenuOpen(false);
                    data.onClick(id);
                  }}
                >
                  <Icon name="xp-edit" />
                  Edit
                </button>
                <div className={styles.menuDivider} />
                <button
                  className={`${styles.menuItemDelete} ${data.stepType === "Condition" && data.hasChildren ? styles.menuItemDisabled : ""}`}
                  disabled={data.stepType === "Condition" && data.hasChildren}
                  title={
                    data.stepType === "Condition" && data.hasChildren
                      ? "You cannot delete condition steps when both the true and false paths are connected to follow-up steps. One of the paths must be empty. The remaining path is then connected to the preceding step after the condition is deleted."
                      : undefined
                  }
                  onClick={(e) => {
                    e.stopPropagation();
                    setMenuOpen(false);
                    data.onDelete(id);
                  }}
                >
                  <Icon name="xp-bin" />
                  Delete
                </button>
              </div>
            )}
          </div>
        )}
      </div>
      {data.stepType !== "Finish" &&
        data.stepType !== "End" &&
        data.stepType !== "Condition" && (
          <Handle type="source" position={Position.Bottom} />
        )}
      {data.stepType === "Condition" && (
        <div className={styles.conditionHandles}>
          <div className={styles.conditionHandleWrap}>
            <div className={styles.conditionHandleTrue}>
              <svg width="10" height="10" viewBox="0 0 16 16" fill="none">
                <path
                  d="M2 8.5L6 12.5L14 3.5"
                  stroke="#fff"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </div>
            <Handle
              type="source"
              position={Position.Bottom}
              id="true"
              className={styles.conditionHandleDot}
              style={{ position: "relative", transform: "none" }}
            />
          </div>
          <div className={styles.conditionHandleWrap}>
            <div className={styles.conditionHandleFalse}>
              <svg width="8" height="8" viewBox="0 0 16 16" fill="none">
                <path
                  d="M3 3L13 13M13 3L3 13"
                  stroke="#fff"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                />
              </svg>
            </div>
            <Handle
              type="source"
              position={Position.Bottom}
              id="false"
              className={styles.conditionHandleDot}
              style={{ position: "relative", transform: "none" }}
            />
          </div>
        </div>
      )}
    </div>
  );
}

interface PlaceholderNodeData {
  label: string;
  onClick: () => void;
  [key: string]: unknown;
}

function PlaceholderNodeComponent({
  data,
}: NodeProps<Node<PlaceholderNodeData>>) {
  return (
    <div className={styles.placeholderNode} onClick={data.onClick}>
      <Handle type="target" position={Position.Top} />
      <svg
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          d="M8 1v14M1 8h14"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
        />
      </svg>
      <span className={styles.placeholderLabel}>{data.label}</span>
    </div>
  );
}

/* Custom edge with "+" add-step button rendered as edge label (matches native) */
interface AddButtonEdgeData {
  onClick: () => void;
  [key: string]: unknown;
}

function AddButtonEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  markerEnd,
  style,
  data,
}: EdgeProps<Edge<AddButtonEdgeData>>) {
  const dx = targetX - sourceX;
  const dy = targetY - sourceY;

  let edgePath: string;
  let labelX: number;
  let labelY: number;

  if (Math.abs(dx) < 1) {
    // Straight vertical edge
    edgePath = `M${sourceX} ${sourceY}L${sourceX} ${targetY}`;
    labelX = sourceX;
    labelY = (sourceY + targetY) / 2;
  } else {
    // L-shaped edge: DOWN → HORIZONTAL → DOWN (matches native routing)
    const dir = Math.sign(dx);
    const turnY = sourceY + Math.min(15, dy * 0.35);
    const r = Math.min(
      10,
      Math.abs(dx) / 2,
      Math.max(1, turnY - sourceY),
      Math.max(1, targetY - turnY),
    );
    edgePath = [
      `M${sourceX} ${sourceY}`,
      `L ${sourceX},${turnY - r}`,
      `Q ${sourceX},${turnY} ${sourceX + dir * r},${turnY}`,
      `L ${targetX - dir * r},${turnY}`,
      `Q ${targetX},${turnY} ${targetX},${turnY + r}`,
      `L${targetX} ${targetY}`,
    ].join("");
    labelX = (sourceX + targetX) / 2;
    labelY = turnY;
  }

  return (
    <>
      <BaseEdge id={id} path={edgePath} markerEnd={markerEnd} style={style} />
      <EdgeLabelRenderer>
        <div
          className="nodrag nopan"
          style={{
            position: "absolute",
            transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`,
            pointerEvents: "all",
          }}
        >
          <button
            className={styles.addBtn}
            onClick={data?.onClick}
            title="Add step"
          >
            <svg
              width="10"
              height="10"
              viewBox="0 0 16 16"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path
                d="M8 1v14M1 8h14"
                stroke="currentColor"
                strokeWidth="3"
                strokeLinecap="round"
              />
            </svg>
          </button>
        </div>
      </EdgeLabelRenderer>
    </>
  );
}

const nodeTypes: NodeTypes = {
  stepNode: StepNodeComponent as any,
  placeholderNode: PlaceholderNodeComponent as any,
};

const edgeTypes: EdgeTypes = {
  addButtonEdge: AddButtonEdge as any,
};

/* ------------------------------------------------------------------ */
/*  Tile picker dialog (shared for trigger & step selection)           */
/* ------------------------------------------------------------------ */

interface TilePickerDialogProps<T> {
  title: string;
  items: T[];
  getKey: (item: T) => string;
  getLabel: (item: T) => string;
  getIcon: (item: T) => string;
  onSelect: (item: T) => void;
  onClose: () => void;
}

function TilePickerDialog<T>({
  title,
  items,
  getKey,
  getLabel,
  getIcon,
  onSelect,
  onClose,
}: TilePickerDialogProps<T>) {
  const [search, setSearch] = useState("");

  const filtered = useMemo(() => {
    if (!search.trim()) return items;
    const q = search.toLowerCase();
    return items.filter((item) => getLabel(item).toLowerCase().includes(q));
  }, [items, search, getLabel]);

  return (
    <div className={styles.dialogOverlay} onClick={onClose}>
      <div
        className={styles.dialogContent}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={styles.dialogHeader}>
          <h3 className={styles.dialogTitle}>{title}</h3>
          <button
            className={styles.dialogClose}
            onClick={onClose}
            title="Close"
          >
            <Icon name="xp-cancel" />
          </button>
        </div>
        <input
          className={styles.searchInput}
          type="text"
          placeholder="Search..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          autoFocus
        />
        <div className={styles.tileGrid}>
          {filtered.map((item) => (
            <div
              key={getKey(item)}
              className={styles.tileItem}
              onClick={() => onSelect(item)}
            >
              <Icon name={getIcon(item) as any} />
              <span className={styles.tileLabel}>{getLabel(item)}</span>
            </div>
          ))}
          {filtered.length === 0 && (
            <div
              style={{
                gridColumn: "1 / -1",
                textAlign: "center",
                color: "#8b8b8b",
                padding: "1rem",
              }}
            >
              No results found
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  AutomationProcessBuilder — forwardRef component with React Flow    */
/* ------------------------------------------------------------------ */

interface AutomationProcessBuilderProps {
  isProcessEnabled: boolean;
  hasHistoryData: boolean;
  onSave: (
    nodes: AutomationProcessNodeDto[],
    connections: AutomationProcessConnectionDto[],
  ) => Promise<boolean>;
  onChange: () => void;
  disabled: boolean;
}

interface AutomationProcessBuilderRef {
  save: () => Promise<void>;
}

const AutomationProcessBuilder = forwardRef<
  AutomationProcessBuilderRef,
  AutomationProcessBuilderProps
>(function AutomationProcessBuilder(
  { isProcessEnabled, hasHistoryData, onSave, onChange, disabled },
  ref,
) {
  const { executeCommand } = usePageCommandProvider();
  const { loadAutomationProcess } = useAutomationCommands();

  // Process data (source of truth)
  const [processNodes, setProcessNodes] = useState<AutomationProcessNodeDto[]>(
    [],
  );
  const [processConnections, setProcessConnections] = useState<
    AutomationProcessConnectionDto[]
  >([]);
  const [isLoading, setIsLoading] = useState(true);

  // Tile items
  const [stepTypes, setStepTypes] = useState<StepTypeTileItem[]>([]);
  const [triggerTypes, setTriggerTypes] = useState<TriggerTypeTileItem[]>([]);

  // Dialog state
  const [pickerMode, setPickerMode] = useState<"trigger" | "step" | null>(null);
  const [insertIndex, setInsertIndex] = useState<number | null>(null);
  const [isFormDialogOpen, setIsFormDialogOpen] = useState(false);
  const [selectedStep, setSelectedStep] =
    useState<AutomationProcessNodeDto | null>(null);
  const [formDefinition, setFormDefinition] =
    useState<GetStepFormDefinitionResult | null>(null);
  const [formValues, setFormValues] = useState<Record<string, string>>({});

  // Pending node — not yet committed to processNodes (added only on Apply)
  const pendingNodeRef = useRef<{
    node: AutomationProcessNodeDto;
    insertIndex: number | null; // null = prepend (trigger), number = insert after index (step)
    branchHandle?: string; // "true" or "false" for condition branches
    branchParentId?: string; // parent condition node ID
  } | null>(null);

  // Identifiers section state
  const [isIdentifiersCollapsed, setIsIdentifiersCollapsed] = useState(true);
  const [autoCodeName, setAutoCodeName] = useState(true);

  // Condition builder state
  const [conditionPanelOpen, setConditionPanelOpen] = useState(false);
  const [conditionPickerOpen, setConditionPickerOpen] = useState(false);
  const [conditionRules, setConditionRules] = useState<ConditionRuleItem[]>([]);
  const [conditionCategories, setConditionCategories] = useState<
    ConditionRuleCategory[]
  >([]);
  const [conditionPickerCategory, setConditionPickerCategory] =
    useState("__all");
  const [conditionPickerSearch, setConditionPickerSearch] = useState("");
  const [conditionGroups, setConditionGroups] = useState<SelectedCondition[][]>(
    [],
  );
  const [groupsOperator, setGroupsOperator] = useState<"all" | "any">("all");
  const [perGroupOperators, setPerGroupOperators] = useState<("all" | "any")[]>(
    [],
  );
  const [addingToGroupIndex, setAddingToGroupIndex] = useState<number>(-1);
  const conditionFieldRef = useRef<string>("");

  // Email selector state
  const [emailSelectorOpen, setEmailSelectorOpen] = useState(false);
  const [emailList, setEmailList] = useState<EmailSelectorItem[]>([]);
  const [emailChannels, setEmailChannels] = useState<EmailChannelOption[]>([]);
  const [emailChannelFilter, setEmailChannelFilter] = useState<string>("");
  const [selectedEmailGuid, setSelectedEmailGuid] = useState<string>("");
  const emailFieldRef = useRef<string>("");
  const conditionParamCallbackRef = useRef<((value: string) => void) | null>(
    null,
  );

  // Wait-before-condition dialog state
  const [waitBeforeConditionOpen, setWaitBeforeConditionOpen] = useState(false);
  const pendingConditionRef = useRef<{
    committedId: string;
    insertIndex: number | null;
    branchHandle?: string;
    branchParentId?: string;
  } | null>(null);

  // React Flow state
  const [rfNodes, setRfNodes, onRfNodesChange] = useNodesState([]);
  const [rfEdges, setRfEdges, onRfEdgesChange] = useEdgesState([]);

  // Load process on mount
  useEffect(() => {
    let cancelled = false;
    (async () => {
      const result = await loadAutomationProcess();
      if (!cancelled && result) {
        setProcessNodes(result.nodes);
        setProcessConnections(result.connections);
      }
      if (!cancelled) setIsLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [loadAutomationProcess]);

  // Load step types + trigger types (independently to avoid one failure blocking the other)
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const stepResult = await executeCommand<GetStepTypeTileItemsResult>(
          "GetStepTypeTileItems",
        );
        if (!cancelled && stepResult) setStepTypes(stepResult.items);
      } catch {
        /* command may not be available */
      }
      try {
        const triggerResult =
          await executeCommand<GetTriggerTypeTileItemsResult>(
            "GetTriggerTypeTileItems",
          );
        if (!cancelled && triggerResult) setTriggerTypes(triggerResult.items);
      } catch {
        /* command may not be available */
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [executeCommand]);

  // Handlers
  const handleNodeClick = useCallback(
    async (nodeId: string) => {
      if (disabled) return;
      const node = processNodes.find((n) => n.id === nodeId);
      if (!node) return;
      setSelectedStep(node);
      const cmdArgs: Record<string, string> = { stepType: node.stepType };
      if (node.stepType === "Trigger" && node.configuration?.triggerType) {
        cmdArgs.triggerType = node.configuration.triggerType;
      }
      const def = await executeCommand<GetStepFormDefinitionResult>(
        "GetStepFormDefinition",
        cmdArgs,
      );
      if (def) {
        setFormDefinition(def);
        const vals: Record<string, string> = {};
        vals["name"] = node.name || "";
        if (node.configuration) {
          for (const [key, val] of Object.entries(node.configuration)) {
            vals[key] = val != null ? String(val) : "";
          }
        }
        for (const field of def.fields) {
          if (!(field.name in vals) && field.defaultValue) {
            vals[field.name] = field.defaultValue;
          }
        }
        setFormValues(vals);
      }
      // Existing node — collapsed identifiers, no auto code name
      setIsIdentifiersCollapsed(true);
      setAutoCodeName(false);

      // Restore condition groups from configuration
      if (def) {
        const condField = def.fields.find(
          (f) => f.fieldType === "conditionBuilder",
        );
        if (condField && node.configuration?.[condField.name]) {
          try {
            const stored = JSON.parse(
              String(node.configuration[condField.name]),
            );
            setConditionGroups(stored.conditionGroups ?? []);
            setGroupsOperator(stored.groupsOperator ?? "all");
            setPerGroupOperators(stored.perGroupOperators ?? []);
          } catch {
            setConditionGroups([]);
            setGroupsOperator("all");
            setPerGroupOperators([]);
          }
        } else {
          setConditionGroups([]);
          setGroupsOperator("all");
          setPerGroupOperators([]);
        }
      }

      setIsFormDialogOpen(true);
    },
    [disabled, processNodes, executeCommand],
  );

  const handleNodeDelete = useCallback(
    (nodeId: string) => {
      if (disabled) return;
      setProcessNodes((prev) => prev.filter((n) => n.id !== nodeId));
      setProcessConnections((prev) =>
        prev.filter(
          (c) => c.sourceNodeGuid !== nodeId && c.targetNodeGuid !== nodeId,
        ),
      );
      onChange();
    },
    [disabled, onChange],
  );

  const handleSidePanelClose = useCallback(() => {
    // Discard pending node if user closes without applying
    pendingNodeRef.current = null;
    setIsFormDialogOpen(false);
    setSelectedStep(null);
    setFormDefinition(null);
    setFormValues({});
    // Reset condition state
    setConditionGroups([]);
    setGroupsOperator("all");
    setPerGroupOperators([]);
    conditionFieldRef.current = "";
  }, []);

  const handleSidePanelApply = useCallback(() => {
    if (!selectedStep) return;
    const { name, ...configFields } = formValues;

    // Serialize condition groups into the conditionBuilder field
    if (formDefinition) {
      const condField = formDefinition.fields.find(
        (f) => f.fieldType === "conditionBuilder",
      );
      if (condField) {
        configFields[condField.name] = JSON.stringify({
          conditionGroups,
          groupsOperator,
          perGroupOperators,
        });
      }
    }

    // Auto-generate codename from step name when autoCodeName is checked
    if (autoCodeName && formDefinition) {
      const codeNameField = formDefinition.fields.find(
        (f) => f.fieldType === "codename",
      );
      if (codeNameField) {
        const stepName = (name as string) || selectedStep.name || "";
        configFields[codeNameField.name] = stepName
          .replace(/[^a-zA-Z0-9_\-. ]/g, "")
          .replace(/\s+/g, "_")
          .replace(/^[.\s]+|[.\s]+$/g, "")
          .substring(0, 100);
      }
    }

    const pending = pendingNodeRef.current;
    if (pending && pending.node.id === selectedStep.id) {
      // Commit the pending node
      const committed = {
        ...pending.node,
        name: name || pending.node.name,
        configuration: configFields,
      };
      if (pending.insertIndex === null) {
        // Trigger — prepend
        setProcessNodes((prev) => [committed, ...prev]);
        // Create connection from trigger to first step (if any)
        setProcessConnections((prev) => {
          const next = [...prev];
          // Find first non-trigger node to connect to
          // (processNodes hasn't updated yet, so use current state)
          if (processNodes.length > 0) {
            const firstStep = processNodes.find(
              (n) => n.stepType !== "Trigger" && n.stepType !== "Start",
            );
            if (firstStep) {
              next.push({
                id: `${committed.id}->${firstStep.id}`,
                sourceNodeGuid: committed.id,
                targetNodeGuid: firstStep.id,
              });
            }
          }
          return next;
        });
      } else if (pending.branchHandle && pending.branchParentId) {
        // Adding to a condition branch
        setProcessNodes((prev) => [...prev, committed]);
        setProcessConnections((prev) => [
          ...prev,
          {
            id: `${pending.branchParentId}-[${pending.branchHandle}]->${committed.id}`,
            sourceNodeGuid: pending.branchParentId!,
            targetNodeGuid: committed.id,
            sourceHandle: pending.branchHandle,
          },
        ]);
      } else {
        // Step — insert after index (linear)
        setProcessNodes((prev) => {
          const next = [...prev];
          next.splice(pending.insertIndex! + 1, 0, committed);
          return next;
        });
        // Update connections: splice new node between prev and next
        setProcessConnections((prev) => {
          const prevNode = processNodes[pending.insertIndex!];
          const nextNode = processNodes[pending.insertIndex! + 1];
          const next = [...prev];

          if (prevNode && nextNode) {
            // Remove old edge from prevNode → nextNode
            const oldIdx = next.findIndex(
              (c) =>
                c.sourceNodeGuid === prevNode.id &&
                c.targetNodeGuid === nextNode.id,
            );
            if (oldIdx >= 0) next.splice(oldIdx, 1);

            // Add edges: prevNode → committed → nextNode
            next.push({
              id: `${prevNode.id}->${committed.id}`,
              sourceNodeGuid: prevNode.id,
              targetNodeGuid: committed.id,
            });
            next.push({
              id: `${committed.id}->${nextNode.id}`,
              sourceNodeGuid: committed.id,
              targetNodeGuid: nextNode.id,
            });
          } else if (prevNode) {
            // Appending after last node
            next.push({
              id: `${prevNode.id}->${committed.id}`,
              sourceNodeGuid: prevNode.id,
              targetNodeGuid: committed.id,
            });
          }
          return next;
        });
      }
      pendingNodeRef.current = null;
      // After committing a new Condition step, show "Add Wait?" dialog
      if (committed.stepType === "Condition") {
        onChange();
        handleSidePanelClose();
        pendingConditionRef.current = {
          committedId: committed.id,
          insertIndex: pending.insertIndex,
          branchHandle: pending.branchHandle,
          branchParentId: pending.branchParentId,
        };
        setWaitBeforeConditionOpen(true);
        return;
      }
    } else {
      // Editing an existing node
      setProcessNodes((prev) =>
        prev.map((n) =>
          n.id === selectedStep.id
            ? { ...n, name: name || n.name, configuration: configFields }
            : n,
        ),
      );
    }
    onChange();
    handleSidePanelClose();
  }, [
    selectedStep,
    formValues,
    autoCodeName,
    formDefinition,
    conditionGroups,
    groupsOperator,
    perGroupOperators,
    processNodes,
    onChange,
    handleSidePanelClose,
  ]);

  const handleTriggerPlaceholderClick = useCallback(() => {
    if (disabled) return;
    setPickerMode("trigger");
  }, [disabled]);

  const branchInfoRef = useRef<{
    handle?: string;
    parentId?: string;
  } | null>(null);

  const handleAddStepClick = useCallback(
    (index: number, branchHandle?: string, branchParentId?: string) => {
      if (disabled) return;
      setInsertIndex(index);
      branchInfoRef.current = branchHandle
        ? { handle: branchHandle, parentId: branchParentId }
        : null;
      setPickerMode("step");
    },
    [disabled],
  );

  const handleTriggerSelect = useCallback(
    async (trigger: TriggerTypeTileItem) => {
      const newNode: AutomationProcessNodeDto = {
        id: crypto.randomUUID(),
        name: trigger.label,
        stepType: "Trigger",
        iconName: trigger.iconName,
        isSaved: false,
        statistics: [],
        configuration: { triggerType: trigger.triggerType },
      };
      // Don't add to processNodes yet — store as pending until Apply
      pendingNodeRef.current = { node: newNode, insertIndex: null };
      setPickerMode(null);

      // Open sidebar for the new trigger
      setSelectedStep(newNode);
      const def = await executeCommand<GetStepFormDefinitionResult>(
        "GetStepFormDefinition",
        { stepType: "Trigger", triggerType: trigger.triggerType },
      );
      if (def) {
        setFormDefinition(def);
        const vals: Record<string, string> = { name: newNode.name || "" };
        if (newNode.configuration) {
          for (const [key, val] of Object.entries(newNode.configuration)) {
            vals[key] = val != null ? String(val) : "";
          }
        }
        for (const field of def.fields) {
          if (!(field.name in vals) && field.defaultValue) {
            vals[field.name] = field.defaultValue;
          }
        }
        setFormValues(vals);
      }
      // New trigger — auto code name enabled, identifiers collapsed
      setIsIdentifiersCollapsed(true);
      setAutoCodeName(true);
      setConditionGroups([]);
      setGroupsOperator("all");
      setPerGroupOperators([]);
      setIsFormDialogOpen(true);
    },
    [executeCommand],
  );

  const openStepForm = useCallback(
    async (newNode: AutomationProcessNodeDto) => {
      setSelectedStep(newNode);
      const def = await executeCommand<GetStepFormDefinitionResult>(
        "GetStepFormDefinition",
        { stepType: newNode.stepType },
      );
      if (def) {
        setFormDefinition(def);
        const vals: Record<string, string> = { name: newNode.name || "" };
        for (const field of def.fields) {
          if (!(field.name in vals) && field.defaultValue) {
            vals[field.name] = field.defaultValue;
          }
        }
        setFormValues(vals);
      }
      setIsIdentifiersCollapsed(true);
      setAutoCodeName(true);
      setConditionGroups([]);
      setGroupsOperator("all");
      setPerGroupOperators([]);
      setIsFormDialogOpen(true);
    },
    [executeCommand],
  );

  const handleStepTypeSelect = useCallback(
    async (stepType: StepTypeTileItem) => {
      if (insertIndex === null) return;
      const capturedInsertIndex = insertIndex;
      const capturedBranch = branchInfoRef.current
        ? { ...branchInfoRef.current }
        : null;

      const newNode: AutomationProcessNodeDto = {
        id: crypto.randomUUID(),
        name: stepType.label,
        stepType: stepType.stepType,
        iconName: stepType.iconName,
        isSaved: false,
        statistics: [],
        configuration: null,
      };

      pendingNodeRef.current = {
        node: newNode,
        insertIndex: capturedInsertIndex,
        branchHandle: capturedBranch?.handle,
        branchParentId: capturedBranch?.parentId,
      };
      branchInfoRef.current = null;
      setPickerMode(null);
      setInsertIndex(null);
      await openStepForm(newNode);
    },
    [insertIndex, openStepForm],
  );

  // Expose save via ref
  useImperativeHandle(
    ref,
    () => ({
      save: async () => {
        await onSave(processNodes, processConnections);
      },
    }),
    [onSave, processNodes, processConnections],
  );

  // Build React Flow nodes/edges from process data (graph-based layout)
  useEffect(() => {
    const BRANCH_OFFSET = 160; // horizontal offset for true/false branches

    const hasTrigger = processNodes.some(
      (n) => n.stepType === "Trigger" || n.stepType === "Start",
    );

    // Build node lookup and adjacency maps from processConnections
    const nodeMap = new Map<string, AutomationProcessNodeDto>();
    for (const n of processNodes) nodeMap.set(n.id, n);

    const forwardMap = new Map<
      string,
      Array<{ target: string; sourceHandle?: string | null }>
    >();
    const incomingSet = new Set<string>();
    for (const conn of processConnections) {
      const arr = forwardMap.get(conn.sourceNodeGuid) || [];
      arr.push({
        target: conn.targetNodeGuid,
        sourceHandle: conn.sourceHandle,
      });
      forwardMap.set(conn.sourceNodeGuid, arr);
      incomingSet.add(conn.targetNodeGuid);
    }

    const flowNodes: Node[] = [];
    const flowEdges: Edge[] = [];
    const visitedNodes = new Set<string>();

    // Recursive graph walker that positions nodes
    const layoutSubtree = (
      nodeId: string,
      x: number,
      y: number,
      parentId: string | null,
      parentSourceHandle?: string | null,
    ): number => {
      if (visitedNodes.has(nodeId)) return y;
      visitedNodes.add(nodeId);

      const node = nodeMap.get(nodeId);
      if (!node) return y;

      const nodeIndex = processNodes.indexOf(node);

      flowNodes.push({
        id: nodeId,
        type: "stepNode",
        position: { x: x - NODE_WIDTH / 2, y },
        data: {
          label: node.name,
          iconName: node.iconName,
          stepType: node.stepType,
          isTrigger: node.stepType === "Trigger" || node.stepType === "Start",
          disabled,
          hasChildren:
            node.stepType === "Condition"
              ? (() => {
                  const conns = forwardMap.get(nodeId) || [];
                  const trueHasChild = conns.some(
                    (c) => c.sourceHandle === "true" && nodeMap.has(c.target),
                  );
                  const falseHasChild = conns.some(
                    (c) => c.sourceHandle === "false" && nodeMap.has(c.target),
                  );
                  return trueHasChild && falseHasChild;
                })()
              : false,
          onDelete: handleNodeDelete,
          onClick: handleNodeClick,
        },
        draggable: false,
        selectable: false,
      });

      // Edge from parent
      if (parentId) {
        const edgeId = parentSourceHandle
          ? `${parentId}-[${parentSourceHandle}]->${nodeId}`
          : `${parentId}->${nodeId}`;
        flowEdges.push({
          id: edgeId,
          source: parentId,
          sourceHandle: parentSourceHandle || undefined,
          target: nodeId,
          type: !disabled ? "addButtonEdge" : undefined,
          markerEnd: {
            type: MarkerType.Arrow,
            width: 20,
            height: 20,
            color: "var(--color-border-default)",
          },
          style: {
            stroke: "var(--color-border-default, #8c8c8c)",
            strokeWidth: 1,
          },
          data: !disabled
            ? {
                onClick: () =>
                  handleAddStepClick(
                    nodeIndex > 0 ? nodeIndex - 1 : 0,
                    parentSourceHandle || undefined,
                    parentId,
                  ),
              }
            : undefined,
        } as Edge);
      }

      let nextY = y + NODE_HEIGHT + NODE_SPACING_Y;
      const children = forwardMap.get(nodeId) || [];

      if (node.stepType === "Condition") {
        // Condition node — two branches
        const trueChild = children.find((c) => c.sourceHandle === "true");
        const falseChild = children.find((c) => c.sourceHandle === "false");

        if (trueChild && nodeMap.has(trueChild.target)) {
          nextY = layoutSubtree(
            trueChild.target,
            x - BRANCH_OFFSET,
            nextY,
            nodeId,
            "true",
          );
        } else if (!disabled) {
          // True placeholder
          const truePhId = `__true_ph_${nodeId}__`;
          flowNodes.push({
            id: truePhId,
            type: "placeholderNode",
            position: { x: x - BRANCH_OFFSET - NODE_WIDTH / 2, y: nextY },
            data: {
              label: "Add step",
              onClick: () => handleAddStepClick(nodeIndex, "true", nodeId),
            },
            draggable: false,
            selectable: false,
          });
          flowEdges.push({
            id: `${nodeId}-[true]->${truePhId}`,
            source: nodeId,
            sourceHandle: "true",
            target: truePhId,
            type: "addButtonEdge",
            markerEnd: {
              type: MarkerType.Arrow,
              width: 20,
              height: 20,
              color: "var(--color-border-default)",
            },
            style: {
              stroke: "var(--color-border-default, #8c8c8c)",
              strokeWidth: 1,
            },
            data: {
              onClick: () => handleAddStepClick(nodeIndex, "true", nodeId),
            },
          } as Edge);
        }

        if (falseChild && nodeMap.has(falseChild.target)) {
          const falseY = layoutSubtree(
            falseChild.target,
            x + BRANCH_OFFSET,
            y + NODE_HEIGHT + NODE_SPACING_Y,
            nodeId,
            "false",
          );
          nextY = Math.max(nextY, falseY);
        } else if (!disabled) {
          // False placeholder
          const falsePhId = `__false_ph_${nodeId}__`;
          flowNodes.push({
            id: falsePhId,
            type: "placeholderNode",
            position: {
              x: x + BRANCH_OFFSET - NODE_WIDTH / 2,
              y: y + NODE_HEIGHT + NODE_SPACING_Y,
            },
            data: {
              label: "Add step",
              onClick: () => handleAddStepClick(nodeIndex, "false", nodeId),
            },
            draggable: false,
            selectable: false,
          });
          flowEdges.push({
            id: `${nodeId}-[false]->${falsePhId}`,
            source: nodeId,
            sourceHandle: "false",
            target: falsePhId,
            type: "addButtonEdge",
            markerEnd: {
              type: MarkerType.Arrow,
              width: 20,
              height: 20,
              color: "var(--color-border-default)",
            },
            style: {
              stroke: "var(--color-border-default, #8c8c8c)",
              strokeWidth: 1,
            },
            data: {
              onClick: () => handleAddStepClick(nodeIndex, "false", nodeId),
            },
          } as Edge);
        }
      } else if (children.length > 0) {
        // Linear node — follow first child
        nextY = layoutSubtree(children[0].target, x, nextY, nodeId);
      } else if (
        node.stepType !== "Finish" &&
        node.stepType !== "End" &&
        !disabled
      ) {
        // Leaf node — add "Add step" placeholder
        const phId = `__step_ph_${nodeId}__`;
        flowNodes.push({
          id: phId,
          type: "placeholderNode",
          position: { x: x - NODE_WIDTH / 2, y: nextY },
          data: {
            label: "Add step",
            onClick: () => handleAddStepClick(nodeIndex),
          },
          draggable: false,
          selectable: false,
        });
        flowEdges.push({
          id: `${nodeId}->${phId}`,
          source: nodeId,
          target: phId,
          markerEnd: {
            type: MarkerType.Arrow,
            width: 20,
            height: 20,
            color: "var(--color-border-default)",
          },
          style: {
            stroke: "var(--color-border-default, #8c8c8c)",
            strokeWidth: 1,
          },
        } as Edge);
      }

      return nextY;
    };

    const centerX = 0;
    let y = 0;

    // Add trigger placeholder if needed
    if (!hasTrigger && !disabled) {
      const placeholderId = "__trigger_placeholder__";
      flowNodes.push({
        id: placeholderId,
        type: "placeholderNode",
        position: { x: centerX - NODE_WIDTH / 2, y },
        data: {
          label: "Add trigger",
          onClick: handleTriggerPlaceholderClick,
        },
        draggable: false,
        selectable: false,
      });
      y += NODE_HEIGHT + NODE_SPACING_Y;
    }

    // Find root node (no incoming connections) and walk graph
    if (processNodes.length > 0) {
      const rootNode =
        processNodes.find((n) => !incomingSet.has(n.id)) || processNodes[0];
      if (hasTrigger && !incomingSet.has(rootNode.id)) {
        // Trigger has no parent edge — layout from it
        layoutSubtree(rootNode.id, centerX, y, null);
      } else if (!hasTrigger && !disabled) {
        // Connect trigger placeholder to first node
        layoutSubtree(rootNode.id, centerX, y, "__trigger_placeholder__");
      } else {
        layoutSubtree(rootNode.id, centerX, y, null);
      }

      // Layout any nodes not yet visited (disconnected)
      for (const node of processNodes) {
        if (!visitedNodes.has(node.id)) {
          // Fallback — should not normally happen
          layoutSubtree(node.id, centerX, y, null);
        }
      }
    }

    setRfNodes(flowNodes);
    setRfEdges(flowEdges);
  }, [
    processNodes,
    processConnections,
    disabled,
    handleNodeDelete,
    handleNodeClick,
    handleTriggerPlaceholderClick,
    handleAddStepClick,
    setRfNodes,
    setRfEdges,
  ]);

  /** Renders a single selected condition with inline parameter controls. */
  const renderConditionInline = (
    cond: SelectedCondition,
    idx: number,
    onRemove: () => void,
    onParamChange: (paramName: string, value: string) => void,
  ) => {
    // Parse ruleText "Contact field {field} {op} {value}" into segments
    const segments: { type: "text" | "param"; value: string }[] = [];
    const regex = /\{([^}]+)\}/g;
    let lastIndex = 0;
    let match: RegExpExecArray | null;
    while ((match = regex.exec(cond.ruleItem.ruleText)) !== null) {
      if (match.index > lastIndex) {
        segments.push({
          type: "text",
          value: cond.ruleItem.ruleText.slice(lastIndex, match.index).trim(),
        });
      }
      segments.push({ type: "param", value: match[1] });
      lastIndex = match.index + match[0].length;
    }
    if (lastIndex < cond.ruleItem.ruleText.length) {
      const trailing = cond.ruleItem.ruleText.slice(lastIndex).trim();
      if (trailing) segments.push({ type: "text", value: trailing });
    }

    return (
      <Condition
        key={idx}
        deleteButton={{ tooltipText: "Remove", onDelete: onRemove }}
      >
        <div className={styles.conditionParamsRow}>
          {segments.map((seg, sIdx) => {
            if (seg.type === "text") {
              return (
                <span key={sIdx} className={styles.conditionParamLabel}>
                  {seg.value}
                </span>
              );
            }
            const param = cond.ruleItem.parameters.find(
              (p) => p.name === seg.value,
            );
            if (!param) return null;

            console.log(
              "PARAM DEBUG",
              param.name,
              param.controlType,
              param.options?.length,
            );

            if (
              param.controlType === "selectExisting" &&
              param.options &&
              param.options.length > 0
            ) {
              const currentVal =
                cond.paramValues[param.name] ?? param.defaultValue ?? "";
              const selectedOpt = param.options.find(
                (o) => o.value === currentVal,
              );
              return (
                <div key={sIdx} className={styles.objectSelector}>
                  {selectedOpt ? (
                    <div className={styles.objectSelectorSelected}>
                      <span className={styles.objectSelectorSelectedName}>
                        {selectedOpt.label}
                      </span>
                      <button
                        type="button"
                        className={styles.objectSelectorClearBtn}
                        onClick={() => onParamChange(param.name, "")}
                        title="Remove"
                      >
                        <Icon name={"xp-bin" as any} />
                      </button>
                    </div>
                  ) : null}
                  <Button
                    label="Select existing"
                    color={ButtonColor.Secondary}
                    size={ButtonSize.S}
                    onClick={() => {
                      conditionParamCallbackRef.current = (val: string) =>
                        onParamChange(param.name, val);
                      setEmailSelectorOpen(true);
                      if (emailList.length === 0) {
                        executeCommand<GetEmailsForSelectorResult>(
                          "GetEmailsForSelector",
                        ).then((result) => {
                          if (result) {
                            setEmailList(result.emails);
                            setEmailChannels(result.channels);
                            if (
                              result.channels.length > 0 &&
                              !emailChannelFilter
                            ) {
                              setEmailChannelFilter(
                                String(result.channels[0].id),
                              );
                            }
                          }
                        });
                      }
                    }}
                  />
                </div>
              );
            }

            if (
              param.controlType === "dropdown" &&
              param.options &&
              param.options.length > 0
            ) {
              return (
                <Select
                  key={sIdx}
                  placeholder={param.placeholder ?? param.caption}
                  value={
                    cond.paramValues[param.name] ?? param.defaultValue ?? ""
                  }
                  onChange={(val) => onParamChange(param.name, val ?? "")}
                >
                  {param.options.map((opt) => (
                    <MenuItem
                      key={opt.value}
                      primaryLabel={opt.label}
                      value={opt.value}
                    />
                  ))}
                </Select>
              );
            }

            return (
              <input
                key={sIdx}
                type={param.controlType === "number" ? "number" : "text"}
                className={styles.conditionParamInput}
                placeholder={param.placeholder ?? param.caption}
                value={cond.paramValues[param.name] ?? ""}
                onChange={(e) => onParamChange(param.name, e.target.value)}
              />
            );
          })}
        </div>
      </Condition>
    );
  };

  if (isLoading) {
    return (
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          height: "100%",
          color: "var(--color-text-low-emphasis, #8b8b8b)",
        }}
      >
        Loading...
      </div>
    );
  }

  return (
    <>
      {/* React Flow canvas */}
      <div className={styles.canvasWrapper}>
        <ReactFlow
          nodes={rfNodes}
          edges={rfEdges}
          onNodesChange={onRfNodesChange}
          onEdgesChange={onRfEdgesChange}
          nodeTypes={nodeTypes}
          edgeTypes={edgeTypes}
          fitView
          fitViewOptions={{ padding: 0.3 }}
          nodesDraggable={false}
          nodesConnectable={false}
          elementsSelectable={false}
          panOnDrag
          zoomOnScroll
          minZoom={0.3}
          maxZoom={1.5}
          proOptions={{ hideAttribution: true }}
        >
          <Background gap={16} size={1} />
          <Controls showInteractive={false} />
        </ReactFlow>
      </div>

      {/* Trigger type picker dialog */}
      {pickerMode === "trigger" && (
        <TilePickerDialog
          title="Select trigger type"
          items={triggerTypes}
          getKey={(t) => t.triggerType}
          getLabel={(t) => t.label}
          getIcon={(t) => t.iconName}
          onSelect={handleTriggerSelect}
          onClose={() => setPickerMode(null)}
        />
      )}

      {/* Step type picker dialog */}
      {pickerMode === "step" && (
        <TilePickerDialog
          title="Select step type"
          items={stepTypes}
          getKey={(s) => s.stepType}
          getLabel={(s) => s.label}
          getIcon={(s) => s.iconName}
          onSelect={handleStepTypeSelect}
          onClose={() => {
            setPickerMode(null);
            setInsertIndex(null);
          }}
        />
      )}

      {/* Wait-before-condition confirmation dialog */}
      {waitBeforeConditionOpen && (
        <ConfirmationDialog
          headline="Add a Wait step before the condition?"
          confirmationButtonLabel="Add wait step"
          isConfirmationButtonDestructive={false}
          onCancellation={() => {
            setWaitBeforeConditionOpen(false);
            pendingConditionRef.current = null;
          }}
          onConfirmation={() => {
            const ref = pendingConditionRef.current;
            setWaitBeforeConditionOpen(false);
            pendingConditionRef.current = null;
            if (!ref) return;
            // Insert a Wait node before the just-committed condition
            const waitType = stepTypes.find((s) => s.stepType === "Wait");
            const waitNode: AutomationProcessNodeDto = {
              id: crypto.randomUUID(),
              name: waitType?.label || "Wait",
              stepType: "Wait",
              iconName: waitType?.iconName || "xp-clock",
              isSaved: false,
              statistics: [],
              configuration: { waitTime: "1", waitUnit: "days" },
            };
            if (ref.branchHandle && ref.branchParentId) {
              // Condition was added to a branch — insert wait between branch parent and condition
              setProcessNodes((prev) => [...prev, waitNode]);
              setProcessConnections((prev) => {
                const next = prev.filter(
                  (c) =>
                    !(
                      c.sourceNodeGuid === ref.branchParentId &&
                      c.targetNodeGuid === ref.committedId &&
                      c.sourceHandle === ref.branchHandle
                    ),
                );
                next.push({
                  id: `${ref.branchParentId}-[${ref.branchHandle}]->${waitNode.id}`,
                  sourceNodeGuid: ref.branchParentId!,
                  targetNodeGuid: waitNode.id,
                  sourceHandle: ref.branchHandle,
                });
                next.push({
                  id: `${waitNode.id}->${ref.committedId}`,
                  sourceNodeGuid: waitNode.id,
                  targetNodeGuid: ref.committedId,
                });
                return next;
              });
            } else {
              // Linear — insert wait between prev node and condition
              setProcessNodes((prev) => {
                const condIdx = prev.findIndex((n) => n.id === ref.committedId);
                if (condIdx >= 0) {
                  const next = [...prev];
                  next.splice(condIdx, 0, waitNode);
                  return next;
                }
                return [...prev, waitNode];
              });
              setProcessConnections((prev) => {
                // Find the edge pointing into the condition node
                const inEdgeIdx = prev.findIndex(
                  (c) => c.targetNodeGuid === ref.committedId,
                );
                const next = [...prev];
                if (inEdgeIdx >= 0) {
                  const inEdge = next[inEdgeIdx];
                  // Replace: prevNode → condition  with  prevNode → wait, wait → condition
                  next.splice(inEdgeIdx, 1);
                  next.push({
                    id: `${inEdge.sourceNodeGuid}->${waitNode.id}`,
                    sourceNodeGuid: inEdge.sourceNodeGuid,
                    targetNodeGuid: waitNode.id,
                    sourceHandle: inEdge.sourceHandle,
                  });
                }
                next.push({
                  id: `${waitNode.id}->${ref.committedId}`,
                  sourceNodeGuid: waitNode.id,
                  targetNodeGuid: ref.committedId,
                });
                return next;
              });
            }
            onChange();
          }}
        >
          <span>
            A wait step gives contacts time to perform the evaluated actions.
            For example, if your process sends an email and then immediately
            follows with a condition that checks if a link in the email was
            clicked, contacts will not have any time to receive and read the
            email.
          </span>
        </ConfirmationDialog>
      )}

      {/* Step settings side panel (matches native right-side panel) */}
      {isFormDialogOpen && selectedStep && formDefinition && (
        <div className={styles.sidePanelOverlay} onClick={handleSidePanelClose}>
          <div
            className={styles.sidePanel}
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div className={styles.sidePanelHeader}>
              <span className={styles.sidePanelHeadline}>
                {formDefinition.headline}
              </span>
              <button
                className={styles.sidePanelClose}
                onClick={handleSidePanelClose}
                title="Close"
              >
                <Icon name={"xp-modal-close" as any} />
              </button>
            </div>

            {/* Content — form fields */}
            <div className={styles.sidePanelContent}>
              {formDefinition.fields.map((field) => {
                // Conditional visibility check
                if (field.visibleWhen) {
                  const depValue =
                    formValues[field.visibleWhen.fieldName] ?? "";
                  if (depValue !== field.visibleWhen.value) return null;
                }

                // Collapsible Identifiers section (matches native CodeNameComponent)
                if (field.fieldType === "codename") {
                  const codeNameValue = autoCodeName
                    ? ""
                    : (formValues[field.name] ?? "");
                  const isValidCodeName = (v: string) =>
                    !v || /^[a-zA-Z0-9_\-.]+$/.test(v);
                  const startsOrEndsWithDot = (v: string) =>
                    v.startsWith(".") || v.endsWith(".");
                  const showError =
                    !autoCodeName &&
                    codeNameValue &&
                    (!isValidCodeName(codeNameValue) ||
                      startsOrEndsWithDot(codeNameValue));

                  return (
                    <div key={field.name} className={styles.identifiersSection}>
                      <button
                        type="button"
                        className={styles.identifiersToggle}
                        onClick={() => setIsIdentifiersCollapsed((v) => !v)}
                        title="Toggle identifiers section"
                      >
                        <span>Identifiers</span>
                        <span className={styles.identifiersChevron}>
                          <Icon
                            name={
                              (isIdentifiersCollapsed
                                ? "xp-chevron-down"
                                : "xp-chevron-up") as any
                            }
                          />
                        </span>
                      </button>
                      {!isIdentifiersCollapsed && (
                        <div className={styles.identifiersContent}>
                          {!selectedStep?.isSaved && (
                            <div className={styles.formField}>
                              <Checkbox
                                label="Pre-fill code name automatically"
                                checked={autoCodeName}
                                onChange={(_e, checked) => {
                                  setAutoCodeName(checked);
                                  if (checked) {
                                    setFormValues((prev) => ({
                                      ...prev,
                                      [field.name]: "",
                                    }));
                                  }
                                }}
                              />
                            </div>
                          )}
                          {!autoCodeName && (
                            <div className={styles.formField}>
                              <Input
                                label={field.label}
                                markAsRequired={field.required}
                                type="text"
                                value={codeNameValue}
                                placeholder="e.g. my_code_name"
                                onChange={(e) =>
                                  setFormValues((prev) => ({
                                    ...prev,
                                    [field.name]: (e.target as HTMLInputElement)
                                      .value,
                                  }))
                                }
                              />
                              {showError && (
                                <div className={styles.codeNameError}>
                                  {!isValidCodeName(codeNameValue)
                                    ? "Only alphanumeric characters and some special characters ('_', '-', '.') are allowed."
                                    : "Cannot start or end with '.'."}
                                </div>
                              )}
                              <div className={styles.codeNameHints}>
                                <span>
                                  Allowed: alphanumeric, '_', '-', '.'
                                </span>
                                <span>Cannot start or end with '.'</span>
                              </div>
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  );
                }

                // Regular form fields
                return (
                  <div key={field.name} className={styles.formField}>
                    {field.fieldType === "select" && field.options ? (
                      <>
                        <Select
                          label={field.label}
                          markAsRequired={field.required}
                          value={
                            formValues[field.name] ?? field.defaultValue ?? ""
                          }
                          onChange={(val) =>
                            setFormValues((prev) => ({
                              ...prev,
                              [field.name]: val ?? "",
                            }))
                          }
                          placeholder={field.placeholder ?? "— Select —"}
                        >
                          {field.options.map((opt) => (
                            <MenuItem
                              key={opt.value}
                              primaryLabel={opt.label}
                              value={opt.value}
                            />
                          ))}
                        </Select>
                        {/* "Edit in forms" button — always visible, disabled until form selected */}
                        {field.options?.some((o) => o.editUrl) &&
                          (() => {
                            const selectedVal = formValues[field.name];
                            const selectedOpt = selectedVal
                              ? field.options?.find(
                                  (o) => o.value === selectedVal,
                                )
                              : null;
                            const editUrl = selectedOpt?.editUrl;
                            const ExternalIcon = (
                              <svg
                                width="1em"
                                height="1em"
                                viewBox="0 0 16 16"
                                fill="none"
                                xmlns="http://www.w3.org/2000/svg"
                                role="img"
                                style={{ display: "block" }}
                              >
                                <path
                                  fillRule="evenodd"
                                  clipRule="evenodd"
                                  d="M3.5 2A1.5 1.5 0 0 0 2 3.5v9A1.5 1.5 0 0 0 3.5 14h9a1.5 1.5 0 0 0 1.5-1.5V8.51a.5.5 0 0 1 1 0v3.99a2.5 2.5 0 0 1-2.5 2.5h-9A2.5 2.5 0 0 1 1 12.5v-9A2.5 2.5 0 0 1 3.5 1h4.003a.5.5 0 0 1 0 1H3.5Zm6.5-.5a.5.5 0 0 1 .5-.5h4a.5.5 0 0 1 .5.5v3.998a.5.5 0 0 1-1 0v-2.79L8.944 7.762a.5.5 0 1 1-.707-.707L13.293 2h-2.794a.5.5 0 0 1-.5-.5Z"
                                  fill="currentColor"
                                />
                              </svg>
                            );
                            return editUrl ? (
                              <a
                                href={editUrl}
                                className={styles.editInFormsLink}
                                target="_blank"
                                rel="noopener noreferrer"
                              >
                                EDIT IN FORMS
                                {ExternalIcon}
                              </a>
                            ) : (
                              <span
                                className={`${styles.editInFormsLink} ${styles.editInFormsLinkDisabled}`}
                              >
                                EDIT IN FORMS
                                {ExternalIcon}
                              </span>
                            );
                          })()}
                      </>
                    ) : field.fieldType === "checkbox" ? (
                      <Checkbox
                        label={field.label}
                        markAsRequired={field.required}
                        checked={formValues[field.name] === "true"}
                        onChange={(_e, checked) =>
                          setFormValues((prev) => ({
                            ...prev,
                            [field.name]: String(checked),
                          }))
                        }
                      />
                    ) : field.fieldType === "radio" && field.options ? (
                      <div className={styles.radioGroup}>
                        <div className={styles.radioGroupLabel}>
                          {field.label}
                        </div>
                        {field.options.map((opt) => (
                          <label key={opt.value} className={styles.radioOption}>
                            <input
                              type="radio"
                              name={field.name}
                              value={opt.value}
                              checked={
                                (formValues[field.name] ??
                                  field.defaultValue ??
                                  "") === opt.value
                              }
                              onChange={() =>
                                setFormValues((prev) => ({
                                  ...prev,
                                  [field.name]: opt.value,
                                }))
                              }
                            />
                            <span className={styles.radioCircle} />
                            <span>{opt.label}</span>
                          </label>
                        ))}
                      </div>
                    ) : field.fieldType === "datetime" ? (
                      <Input
                        label={field.label}
                        markAsRequired={field.required}
                        type="text"
                        value={formValues[field.name] ?? ""}
                        placeholder="mm/dd/yyyy, hh:mm AM"
                        onChange={(e) =>
                          setFormValues((prev) => ({
                            ...prev,
                            [field.name]: (e.target as HTMLInputElement).value,
                          }))
                        }
                      />
                    ) : field.fieldType === "objectSelector" ? (
                      <div className={styles.objectSelector}>
                        <div className={styles.objectSelectorLabel}>
                          {field.required && (
                            <span className={styles.requiredMark}>*</span>
                          )}
                          <span>{field.label}</span>
                        </div>
                        {formValues[field.name] ? (
                          <div className={styles.objectSelectorSelected}>
                            <span className={styles.objectSelectorSelectedName}>
                              {emailList.find(
                                (e) => e.guid === formValues[field.name],
                              )?.name ?? formValues[field.name]}
                            </span>
                            <button
                              type="button"
                              className={styles.objectSelectorClearBtn}
                              onClick={() =>
                                setFormValues((prev) => ({
                                  ...prev,
                                  [field.name]: "",
                                }))
                              }
                              title="Remove"
                            >
                              <Icon name={"xp-bin" as any} />
                            </button>
                          </div>
                        ) : null}
                        <Button
                          label="Select existing"
                          color={ButtonColor.Secondary}
                          onClick={() => {
                            emailFieldRef.current = field.name;
                            setEmailSelectorOpen(true);
                            // Load emails if not loaded
                            if (emailList.length === 0) {
                              executeCommand<GetEmailsForSelectorResult>(
                                "GetEmailsForSelector",
                              ).then((result) => {
                                if (result) {
                                  setEmailList(result.emails);
                                  setEmailChannels(result.channels);
                                  // Auto-select first channel like native
                                  if (
                                    result.channels.length > 0 &&
                                    !emailChannelFilter
                                  ) {
                                    setEmailChannelFilter(
                                      String(result.channels[0].id),
                                    );
                                  }
                                }
                              });
                            }
                          }}
                        />
                      </div>
                    ) : field.fieldType === "combobox" && field.options ? (
                      <Select
                        label={field.label}
                        markAsRequired={field.required}
                        value={formValues[field.name] ?? ""}
                        onChange={(val) =>
                          setFormValues((prev) => ({
                            ...prev,
                            [field.name]: val ?? "",
                          }))
                        }
                        placeholder={field.placeholder ?? "Choose an option"}
                      >
                        {field.options.map((opt) => (
                          <MenuItem
                            key={opt.value}
                            primaryLabel={opt.label}
                            value={opt.value}
                          />
                        ))}
                      </Select>
                    ) : field.fieldType === "conditionBuilder" ? (
                      <div className={styles.conditionBuilder}>
                        <div className={styles.conditionBuilderLabel}>
                          {field.required && (
                            <span className={styles.requiredMark}>*</span>
                          )}
                          <span>{field.label}</span>
                        </div>
                        {conditionGroups.length === 0 ? (
                          <div className={styles.conditionBuilderContent}>
                            <span
                              className={styles.conditionBuilderPlaceholder}
                            >
                              Conditions not defined yet.
                            </span>
                            <Button
                              label="Add condition"
                              color={ButtonColor.Secondary}
                              onClick={() => {
                                conditionFieldRef.current = field.name;
                                setConditionPanelOpen(true);
                                // Load rules if not loaded
                                if (conditionRules.length === 0) {
                                  executeCommand<GetConditionRulesResult>(
                                    "GetConditionRules",
                                  ).then((result) => {
                                    if (result) {
                                      setConditionRules(result.items);
                                      setConditionCategories(result.categories);
                                    }
                                  });
                                }
                              }}
                            />
                          </div>
                        ) : (
                          <div className={styles.conditionBuilderContent}>
                            <div>
                              Applies if the following conditions are fulfilled:
                            </div>
                            {conditionGroups.map((group, gIdx) => (
                              <React.Fragment key={gIdx}>
                                {gIdx > 0 && (
                                  <div
                                    style={{
                                      padding: "4px 0",
                                      fontSize: "13px",
                                      color: "var(--color-text-default)",
                                    }}
                                  >
                                    {groupsOperator === "any" ? "Or" : "And"}
                                  </div>
                                )}
                                <ConditionGroupOverview>
                                  {group.map((cond, idx) => {
                                    const parts: React.ReactNode[] = [];
                                    const regex = /\{([^}]+)\}/g;
                                    let lastIndex = 0;
                                    let match: RegExpExecArray | null;
                                    while (
                                      (match = regex.exec(
                                        cond.ruleItem.ruleText,
                                      )) !== null
                                    ) {
                                      if (match.index > lastIndex) {
                                        parts.push(
                                          cond.ruleItem.ruleText.slice(
                                            lastIndex,
                                            match.index,
                                          ),
                                        );
                                      }
                                      const paramName = match[1];
                                      const value = cond.paramValues[paramName];
                                      let displayValue = "?";
                                      if (value) {
                                        const param =
                                          cond.ruleItem.parameters.find(
                                            (p) => p.name === paramName,
                                          );
                                        if (
                                          (param?.controlType === "dropdown" ||
                                            param?.controlType ===
                                              "selectExisting") &&
                                          param.options
                                        ) {
                                          const opt = param.options.find(
                                            (o) => o.value === value,
                                          );
                                          displayValue = opt?.label ?? value;
                                        } else {
                                          displayValue = value;
                                        }
                                      }
                                      parts.push(
                                        <strong
                                          key={`${gIdx}-${idx}-${paramName}`}
                                        >
                                          {displayValue}
                                        </strong>,
                                      );
                                      lastIndex = regex.lastIndex;
                                    }
                                    if (
                                      lastIndex < cond.ruleItem.ruleText.length
                                    ) {
                                      parts.push(
                                        cond.ruleItem.ruleText.slice(lastIndex),
                                      );
                                    }
                                    return <div key={idx}>{parts}</div>;
                                  })}
                                </ConditionGroupOverview>
                              </React.Fragment>
                            ))}
                            <Button
                              label="Edit condition"
                              color={ButtonColor.Secondary}
                              onClick={() => {
                                conditionFieldRef.current = field.name;
                                setConditionPanelOpen(true);
                                if (conditionRules.length === 0) {
                                  executeCommand<GetConditionRulesResult>(
                                    "GetConditionRules",
                                  ).then((result) => {
                                    if (result) {
                                      setConditionRules(result.items);
                                      setConditionCategories(result.categories);
                                    }
                                  });
                                }
                              }}
                            />
                          </div>
                        )}
                      </div>
                    ) : field.fieldType === "number" ? (
                      <Input
                        label={field.label}
                        markAsRequired={field.required}
                        type="number"
                        value={formValues[field.name] ?? ""}
                        placeholder={field.placeholder ?? ""}
                        onChange={(e) =>
                          setFormValues((prev) => ({
                            ...prev,
                            [field.name]: (e.target as HTMLInputElement).value,
                          }))
                        }
                      />
                    ) : (
                      <Input
                        label={field.label}
                        markAsRequired={field.required}
                        type="text"
                        value={formValues[field.name] ?? ""}
                        placeholder={field.placeholder ?? ""}
                        onChange={(e) =>
                          setFormValues((prev) => ({
                            ...prev,
                            [field.name]: (e.target as HTMLInputElement).value,
                          }))
                        }
                      />
                    )}
                    {field.helpText && (
                      <div className={styles.fieldHelpText}>
                        {field.helpText}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>

            {/* Footer — Apply button */}
            <div className={styles.sidePanelFooter}>
              <Button
                label="Apply"
                color={ButtonColor.Primary}
                onClick={handleSidePanelApply}
              />
            </div>
          </div>
        </div>
      )}

      {/* Condition builder — second slide-in panel */}
      {conditionPanelOpen && (
        <div
          className={styles.secondPanelOverlay}
          onClick={() => {
            setConditionPanelOpen(false);
            setConditionPickerOpen(false);
          }}
        >
          <div
            className={styles.secondPanel}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.sidePanelHeader}>
              <span className={styles.sidePanelHeadline}>
                Create your own condition
              </span>
              <button
                className={styles.sidePanelClose}
                onClick={() => {
                  setConditionPanelOpen(false);
                  setConditionPickerOpen(false);
                }}
                title="Close"
              >
                <Icon name={"xp-modal-close" as any} />
              </button>
            </div>
            <div className={styles.sidePanelContent}>
              {conditionPickerOpen ? (
                <ConditionPicker
                  headline="Pick a suitable condition"
                  items={conditionRules
                    .filter(
                      (r) =>
                        conditionPickerCategory === "__all" ||
                        r.categoryId === conditionPickerCategory,
                    )
                    .filter(
                      (r) =>
                        !conditionPickerSearch ||
                        r.label
                          .toLowerCase()
                          .includes(conditionPickerSearch.toLowerCase()),
                    )
                    .map((r) => ({ value: r.value, label: r.label }))}
                  categories={[
                    { id: "__all", label: "All" },
                    ...conditionCategories.map((c) => ({
                      id: c.id,
                      label: c.label,
                    })),
                  ]}
                  selectedCategoryId={conditionPickerCategory}
                  onCategoryChange={(catId) =>
                    setConditionPickerCategory(catId ?? "__all")
                  }
                  searchValue={conditionPickerSearch}
                  onSearch={(val) => setConditionPickerSearch(val ?? "")}
                  noItemsText="No conditions found"
                  searchPlaceholder="Search conditions..."
                  onItemSelect={(item) => {
                    const rule = conditionRules.find(
                      (r) => r.value === item.value,
                    );
                    if (rule) {
                      // Build default param values from parameter definitions
                      const paramValues: Record<string, string> = {};
                      for (const p of rule.parameters) {
                        if (p.defaultValue) {
                          // Default value may be "Value;label" format — extract the value part
                          const semiIdx = p.defaultValue.indexOf(";");
                          paramValues[p.name] =
                            semiIdx >= 0
                              ? p.defaultValue.substring(0, semiIdx)
                              : p.defaultValue;
                        }
                      }
                      const selected: SelectedCondition = {
                        ruleItem: rule,
                        paramValues,
                      };
                      setConditionGroups((prev) => {
                        const gIdx = addingToGroupIndex;
                        if (gIdx >= 0 && gIdx < prev.length) {
                          // Add to existing group
                          return prev.map((g, i) =>
                            i === gIdx ? [...g, selected] : g,
                          );
                        }
                        // New group
                        return [...prev, [selected]];
                      });
                    }
                    setConditionPickerOpen(false);
                    setAddingToGroupIndex(-1);
                  }}
                  onClose={() => {
                    setConditionPickerOpen(false);
                    setAddingToGroupIndex(-1);
                  }}
                />
              ) : (
                <ConditionBuilder
                  actionLabel="Add condition group"
                  isActionVisible={true}
                  onActionClick={() => {
                    setAddingToGroupIndex(-1);
                    setConditionPickerOpen(true);
                  }}
                >
                  {conditionGroups.length >= 2 && (
                    <div className={styles.operatorSelect}>
                      <span>Applies if:</span>
                      <div className={styles.operatorSelectDropdown}>
                        <Select
                          value={groupsOperator}
                          onChange={(val) =>
                            setGroupsOperator(val as "all" | "any")
                          }
                        >
                          <MenuItem primaryLabel="All" value="all" />
                          <MenuItem primaryLabel="Any" value="any" />
                        </Select>
                      </div>
                      <span>
                        of the following condition groups are fulfilled.
                      </span>
                    </div>
                  )}
                  {conditionGroups.map((group, gIdx) => (
                    <React.Fragment key={gIdx}>
                      {gIdx > 0 && (
                        <ConditionOperatorLine
                          operator={groupsOperator === "any" ? "Or" : "And"}
                        />
                      )}
                      <ConditionGroup
                        actionLabel="Add another condition"
                        isActionVisible={true}
                        onActionClick={() => {
                          setAddingToGroupIndex(gIdx);
                          setConditionPickerOpen(true);
                        }}
                      >
                        {group.length >= 2 && (
                          <div className={styles.operatorSelect}>
                            <span>Applies if:</span>
                            <div className={styles.operatorSelectDropdown}>
                              <Select
                                value={perGroupOperators[gIdx] || "all"}
                                onChange={(val) =>
                                  setPerGroupOperators((prev) => {
                                    const updated = [...prev];
                                    updated[gIdx] = val as "all" | "any";
                                    return updated;
                                  })
                                }
                              >
                                <MenuItem primaryLabel="All" value="all" />
                                <MenuItem primaryLabel="Any" value="any" />
                              </Select>
                            </div>
                            <span>
                              of the following conditions are fulfilled.
                            </span>
                            {conditionGroups.length >= 2 && (
                              <button
                                className={styles.deleteGroupButton}
                                title="Delete condition group"
                                onClick={() =>
                                  setConditionGroups((prev) => {
                                    const updated = prev.filter(
                                      (_, i) => i !== gIdx,
                                    );
                                    setPerGroupOperators((ops) =>
                                      ops.filter((_, i) => i !== gIdx),
                                    );
                                    return updated;
                                  })
                                }
                              >
                                <Icon name="xp-bin" />
                              </button>
                            )}
                          </div>
                        )}
                        {group.map((cond, idx) => (
                          <React.Fragment key={idx}>
                            {idx > 0 && (
                              <ConditionOperatorLine
                                operator={
                                  (perGroupOperators[gIdx] || "all") === "any"
                                    ? "Or"
                                    : "And"
                                }
                              />
                            )}
                            {renderConditionInline(
                              cond,
                              idx,
                              () =>
                                setConditionGroups((prev) => {
                                  const updated = prev.map((g, gi) =>
                                    gi === gIdx
                                      ? g.filter((_, ci) => ci !== idx)
                                      : g,
                                  );
                                  // Remove empty groups
                                  return updated.filter((g) => g.length > 0);
                                }),
                              (paramName, value) =>
                                setConditionGroups((prev) =>
                                  prev.map((g, gi) =>
                                    gi === gIdx
                                      ? g.map((c, ci) =>
                                          ci === idx
                                            ? {
                                                ...c,
                                                paramValues: {
                                                  ...c.paramValues,
                                                  [paramName]: value,
                                                },
                                              }
                                            : c,
                                        )
                                      : g,
                                  ),
                                ),
                            )}
                          </React.Fragment>
                        ))}
                      </ConditionGroup>
                    </React.Fragment>
                  ))}
                </ConditionBuilder>
              )}
            </div>
            <div className={styles.sidePanelFooter}>
              <Button
                label="Cancel"
                color={ButtonColor.Secondary}
                onClick={() => {
                  setConditionPanelOpen(false);
                  setConditionPickerOpen(false);
                  setAddingToGroupIndex(-1);
                }}
              />
              <Button
                label="Apply"
                color={ButtonColor.Primary}
                onClick={() => {
                  setConditionPanelOpen(false);
                  setConditionPickerOpen(false);
                  setAddingToGroupIndex(-1);
                }}
              />
            </div>
          </div>
        </div>
      )}

      {/* Email selector — second slide-in panel */}
      {emailSelectorOpen && (
        <div
          className={styles.secondPanelOverlay}
          onClick={() => setEmailSelectorOpen(false)}
        >
          <div
            className={styles.secondPanel}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.sidePanelHeader}>
              <span className={styles.sidePanelHeadline}>Select an email</span>
              <button
                className={styles.sidePanelClose}
                onClick={() => setEmailSelectorOpen(false)}
                title="Close"
              >
                <Icon name={"xp-modal-close" as any} />
              </button>
            </div>
            <div className={styles.sidePanelContent}>
              {emailChannels.length > 0 && (
                <div className={styles.emailChannelFilter}>
                  <Select
                    value={emailChannelFilter}
                    onChange={(val) => setEmailChannelFilter(val ?? "")}
                  >
                    {emailChannels.map((ch) => (
                      <MenuItem
                        key={ch.id}
                        primaryLabel={ch.name}
                        value={String(ch.id)}
                      />
                    ))}
                  </Select>
                </div>
              )}
              <div className={styles.emailSelectedCounter}>
                <span>
                  {selectedEmailGuid ? "1 email selected" : "0 emails selected"}
                </span>
                <button
                  type="button"
                  className={styles.emailToggleButton}
                  aria-label="toggle-button-list"
                >
                  <svg
                    width="1em"
                    height="1em"
                    viewBox="0 0 16 16"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                    role="img"
                    style={{ display: "block" }}
                  >
                    <path
                      d="M5.99 2.5a.5.5 0 0 1 .501-.5H14.5a.5.5 0 1 1 0 1H6.492a.5.5 0 0 1-.5-.5ZM1.505 5.999a.5.5 0 1 0 0 .999h1.001a.5.5 0 1 0 0-1H1.505ZM1.505 10.004a.5.5 0 1 0 0 1h1.001a.5.5 0 1 0 0-1H1.505ZM1 14.5a.5.5 0 0 1 .5-.5h1.002a.5.5 0 1 1 0 1H1.5A.5.5 0 0 1 1 14.5ZM6.491 5.999a.5.5 0 1 0 0 .999H14.5a.5.5 0 1 0 0-1H6.492ZM5.99 10.504a.5.5 0 0 1 .501-.5H14.5a.5.5 0 1 1 0 1H6.492a.5.5 0 0 1-.5-.5ZM6.491 14a.5.5 0 1 0 0 1H14.5a.5.5 0 1 0 0-1H6.492ZM1.5 2a.5.5 0 1 0 0 1h1.002a.5.5 0 1 0 0-1H1.5Z"
                      fill="currentColor"
                    />
                  </svg>
                </button>
              </div>
              {(() => {
                const filteredEmails = emailList.filter(
                  (e) =>
                    !emailChannelFilter ||
                    emailChannels.find(
                      (c) => String(c.id) === emailChannelFilter,
                    )?.name === e.channelName,
                );
                const noChannel = !emailChannelFilter;
                return (
                  <table className={styles.emailTable}>
                    <thead>
                      <tr>
                        <th style={{ width: 40 }}>
                          <Checkbox
                            label=""
                            checked={
                              filteredEmails.length > 0 &&
                              filteredEmails.some(
                                (e) => e.status === "Published",
                              ) &&
                              filteredEmails
                                .filter((e) => e.status === "Published")
                                .every((e) => e.guid === selectedEmailGuid)
                            }
                            disabled={
                              noChannel ||
                              !filteredEmails.some(
                                (e) => e.status === "Published",
                              )
                            }
                            onChange={() => {}}
                          />
                        </th>
                        <th>Email name</th>
                        <th>Purpose</th>
                        <th>Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredEmails.map((email) => {
                        const isUnpublished = email.status !== "Published";
                        const rowDisabled = noChannel || isUnpublished;
                        return (
                          <tr
                            key={email.guid}
                            className={
                              rowDisabled
                                ? styles.emailRowDisabled
                                : selectedEmailGuid === email.guid
                                  ? styles.emailRowSelected
                                  : styles.emailRow
                            }
                            onClick={
                              rowDisabled
                                ? undefined
                                : () => setSelectedEmailGuid(email.guid)
                            }
                          >
                            <td>
                              <Checkbox
                                label=""
                                checked={selectedEmailGuid === email.guid}
                                disabled={rowDisabled}
                                onChange={
                                  rowDisabled
                                    ? () => {}
                                    : () => {
                                        setSelectedEmailGuid(
                                          selectedEmailGuid === email.guid
                                            ? ""
                                            : email.guid,
                                        );
                                      }
                                }
                              />
                            </td>
                            <td>
                              <span className={styles.emailNameCell}>
                                <Icon name={"xp-doc" as any} />
                                {email.name}
                              </span>
                            </td>
                            <td>{email.purpose}</td>
                            <td>
                              <span className={styles.emailStatusCell}>
                                <Icon name={"xp-edit" as any} />
                                {email.status}
                              </span>
                            </td>
                          </tr>
                        );
                      })}
                      {filteredEmails.length === 0 && (
                        <tr>
                          <td colSpan={4} className={styles.emailTableEmpty}>
                            No emails available
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                );
              })()}
            </div>
            <div className={styles.sidePanelFooter}>
              <Button
                label="Cancel"
                color={ButtonColor.Secondary}
                onClick={() => {
                  setEmailSelectorOpen(false);
                  setSelectedEmailGuid("");
                  conditionParamCallbackRef.current = null;
                }}
              />
              <Button
                label="Select"
                color={ButtonColor.Primary}
                disabled={!selectedEmailGuid}
                onClick={() => {
                  if (selectedEmailGuid) {
                    if (conditionParamCallbackRef.current) {
                      conditionParamCallbackRef.current(selectedEmailGuid);
                      conditionParamCallbackRef.current = null;
                    } else {
                      setFormValues((prev) => ({
                        ...prev,
                        [emailFieldRef.current]: selectedEmailGuid,
                      }));
                    }
                  }
                  setEmailSelectorOpen(false);
                  setSelectedEmailGuid("");
                }}
              />
            </div>
          </div>
        </div>
      )}
    </>
  );
});

/* ------------------------------------------------------------------ */
/*  EnableDisableButton — matches native Bd component                  */
/* ------------------------------------------------------------------ */

interface EnableDisableButtonProps {
  isEditingAllowed: boolean;
  isAutomationProcessEnabled: boolean;
  onStatusChange: (isEnabled: boolean) => void;
  onSave: () => Promise<void>;
  isConfirmationDialogShownOnSave: boolean;
  setIsEnableDisableInProgress: (inProgress: boolean) => void;
  disabled: boolean;
  shouldTriggerSaveOnClick: boolean;
}

function EnableDisableButton({
  isEditingAllowed,
  isAutomationProcessEnabled,
  onStatusChange,
  onSave,
  isConfirmationDialogShownOnSave,
  setIsEnableDisableInProgress,
  disabled,
  shouldTriggerSaveOnClick,
}: EnableDisableButtonProps) {
  const { enableAutomationProcess, disableAutomationProcess } =
    useAutomationCommands();
  const [isToggling, setIsToggling] = useState(false);
  const [isSavingThenToggle, setIsSavingThenToggle] = useState(false);
  const [isConfirmationShown, setIsConfirmationShown] = useState(false);

  const label = isAutomationProcessEnabled ? "Disable" : "Enable";

  useEffect(() => {
    setIsEnableDisableInProgress(
      isToggling || isSavingThenToggle || isConfirmationShown,
    );
  }, [
    isToggling,
    isSavingThenToggle,
    setIsEnableDisableInProgress,
    isConfirmationShown,
  ]);

  const tooltip = !isEditingAllowed
    ? "You do not have permission to edit this process."
    : isAutomationProcessEnabled
      ? "Disable this automation process"
      : "Enable this automation process";

  const toggleProcess = useCallback(async () => {
    const result = isAutomationProcessEnabled
      ? await disableAutomationProcess()
      : await enableAutomationProcess();
    if (result) {
      onStatusChange(result.isEnabled);
    }
  }, [
    disableAutomationProcess,
    enableAutomationProcess,
    isAutomationProcessEnabled,
    onStatusChange,
  ]);

  const saveAndToggle = useCallback(async () => {
    await onSave();
    await toggleProcess();
  }, [toggleProcess, onSave]);

  const handleClick = useCallback(async () => {
    if (shouldTriggerSaveOnClick) {
      if (isConfirmationDialogShownOnSave) {
        setIsConfirmationShown(true);
        return;
      }
      setIsToggling(true);
      await saveAndToggle();
      setIsToggling(false);
    } else {
      setIsToggling(true);
      await toggleProcess();
      setIsToggling(false);
    }
  }, [
    shouldTriggerSaveOnClick,
    isConfirmationDialogShownOnSave,
    saveAndToggle,
    toggleProcess,
  ]);

  const handleConfirmation = useCallback(async () => {
    setIsSavingThenToggle(true);
    await saveAndToggle();
    setIsConfirmationShown(false);
    setIsSavingThenToggle(false);
  }, [saveAndToggle]);

  const handleCancellation = useCallback(() => {
    setIsConfirmationShown(false);
  }, []);

  return (
    <>
      <Button
        onClick={handleClick}
        label={label}
        color={ButtonColor.Primary}
        disabled={!isEditingAllowed || disabled || isConfirmationShown}
        inProgress={isToggling}
        title={tooltip}
        dataTestId={TestIds.Buttons.EnableDisableProcess}
      />
      {isConfirmationShown && (
        <ConfirmationDialog
          headline="Enable automation process"
          confirmationButtonLabel="Save &amp; Enable"
          isConfirmationButtonDestructive={false}
          onCancellation={handleCancellation}
          onConfirmation={handleConfirmation}
          actionInProgress={isSavingThenToggle}
        >
          <Box>
            <p>
              The process has unsaved changes. Enabling will save current
              changes first.
            </p>
            <p>
              Once enabled, contacts will start being processed by this
              automation.
            </p>
          </Box>
        </ConfirmationDialog>
      )}
    </>
  );
}

/* ------------------------------------------------------------------ */
/*  AutomationBuilderTemplate — main page template (matches native)    */
/* ------------------------------------------------------------------ */

const SAVE_COMMAND = "Save";

export const AutomationBuilderTemplate: React.FC<AutomationBuilderProps> = (
  props,
) => {
  const {
    isAutomationProcessEnabled,
    isEditingAllowed,
    saveButton,
    hasHistoryData,
  } = props;

  const builderRef = useRef<AutomationProcessBuilderRef>(null);
  const headerRef = useRef(document.getElementById("applicationHeader"));
  const canvasContainerRef = useRef<HTMLDivElement>(null);

  // Fix layout: the framework injects side-menu-wrapper (General link) inside
  // column 1 (scrollable area) alongside our canvas. In native it lives in
  // column 2 (right nav column). Clone it there to match native positioning
  // while keeping the original in the DOM so React can unmount cleanly.
  useEffect(() => {
    const scrollable = canvasContainerRef.current?.parentElement;
    if (!scrollable) return;

    const sideMenuWrapper = scrollable.querySelector<HTMLElement>(
      '[class*="side-menu-wrapper___"]',
    );
    if (!sideMenuWrapper) return;

    // Find column 2: the sibling column containing view-menu-wrapper
    const column1 = scrollable.parentElement;
    const row = column1?.parentElement;
    if (!row) return;

    const column2 = Array.from(row.children).find(
      (col) =>
        col !== column1 && col.querySelector('[class*="view-menu-wrapper___"]'),
    ) as HTMLElement | undefined;

    if (!column2) return;

    // Hide original (keep in DOM for React) and place a clone in column 2
    sideMenuWrapper.style.display = "none";
    const clone = sideMenuWrapper.cloneNode(true) as HTMLElement;
    clone.style.display = "";
    clone.setAttribute("data-cloned", "true");
    column2.appendChild(clone);

    return () => {
      sideMenuWrapper.style.display = "";
      clone.remove();
    };
  }, []);

  const [isSaving, setIsSaving] = useState(false);
  const [isEnableDisableInProgress, setIsEnableDisableInProgress] =
    useState(false);
  const [isConfirmationShown, setIsConfirmationShown] = useState(false);
  const [isSaveInProgress, setIsSaveInProgress] = useState(false);
  const [isEnabled, setIsEnabled] = useState(isAutomationProcessEnabled);
  const { executeCommand } = usePageCommandProvider();
  const { setDataChanged, getNewId } = useEditableObjectStatusObservee();

  const [isDirty, setIsDirty] = useState(false);
  const dirtyIdRef = useRef(getNewId());

  useEffect(() => {
    setDataChanged(dirtyIdRef.current, isDirty);
  }, [isDirty, setDataChanged]);

  const markDirty = useCallback(() => {
    setIsDirty(true);
  }, []);

  // Save handler — sends nodes + connections to server
  const handleSave = useCallback(
    async (
      nodes: AutomationProcessNodeDto[],
      connections: AutomationProcessConnectionDto[],
    ): Promise<boolean> => {
      const result = await executeCommand<AutomationBuilderSaveResult>(
        SAVE_COMMAND,
        { nodes, connections },
      );
      const success = result?.status === FormSubmissionStatus.ValidationSuccess;
      if (success) {
        setIsDirty(false);
      }
      return success;
    },
    [executeCommand],
  );

  // Save button click — may show confirmation dialog
  const handleSaveButtonClick = useCallback(async () => {
    if ((hasHistoryData || isEnabled) && saveButton.confirmationDialog) {
      setIsConfirmationShown(true);
      return;
    }
    setIsSaving(true);
    await builderRef.current?.save();
    setIsSaving(false);
  }, [saveButton.confirmationDialog, hasHistoryData, isEnabled]);

  const handleConfirmation = useCallback(async () => {
    setIsSaveInProgress(true);
    await builderRef.current?.save();
    setIsConfirmationShown(false);
    setIsSaveInProgress(false);
  }, []);

  const handleCancelConfirmation = useCallback(() => {
    setIsConfirmationShown(false);
  }, []);

  const handleStatusChange = useCallback((enabled: boolean) => {
    setIsEnabled(enabled);
  }, []);

  return (
    <>
      {/* Header portal — Save + Enable/Disable buttons */}
      <Portal container={headerRef.current}>
        <Inline spacingX={Spacing.L} className={styles.inline}>
          <Button
            type={ButtonType.Submit}
            label={saveButton.label}
            color={ButtonColor.Secondary}
            onClick={handleSaveButtonClick}
            dataTestId={TestIds.Buttons.SaveAutomation}
            inProgress={isSaving}
            disabled={
              !isEditingAllowed ||
              !isDirty ||
              isEnableDisableInProgress ||
              isConfirmationShown
            }
            title={
              isEditingAllowed && isDirty ? "" : (saveButton.tooltipText ?? "")
            }
          />
          <EnableDisableButton
            isEditingAllowed={isEditingAllowed}
            isAutomationProcessEnabled={isEnabled}
            onStatusChange={handleStatusChange}
            onSave={async () => {
              await builderRef.current?.save();
            }}
            isConfirmationDialogShownOnSave={hasHistoryData}
            setIsEnableDisableInProgress={setIsEnableDisableInProgress}
            disabled={isSaving || isConfirmationShown}
            shouldTriggerSaveOnClick={!isEnabled && isDirty}
          />
        </Inline>
      </Portal>

      {/* Automation process builder canvas + side menu */}
      <div ref={canvasContainerRef} className={styles.canvasFlexWrapper}>
        <AutomationProcessBuilder
          ref={builderRef}
          isProcessEnabled={isEnabled}
          hasHistoryData={hasHistoryData}
          onSave={handleSave}
          onChange={markDirty}
          disabled={!isEditingAllowed}
        />
      </div>
      <TemplateSideMenu />

      {/* Save confirmation dialog */}
      {saveButton.confirmationDialog && isConfirmationShown ? (
        <ConfirmationDialog
          headline={saveButton.confirmationDialog.title ?? ""}
          confirmationButtonLabel={saveButton.confirmationDialog.button}
          isConfirmationButtonDestructive={false}
          onCancellation={handleCancelConfirmation}
          onConfirmation={handleConfirmation}
          actionInProgress={isSaveInProgress}
        >
          {saveButton.confirmationDialog.detail && (
            <Box spacingBottom={Spacing.L}>
              {saveButton.confirmationDialog.detail}
            </Box>
          )}
        </ConfirmationDialog>
      ) : (
        <></>
      )}

      <RoutingContentPlaceholder />
    </>
  );
};
